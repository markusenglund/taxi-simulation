from manim import *

DEFAULT_FONT_SIZE = 50
Text.set_default(font="sans-serif", font_size=DEFAULT_FONT_SIZE)

class WaitingTimeDiagram(Scene):
    def construct(self):
      self.camera.background_color = "#444444"
      
      values=[ 0, 0, 0, 0, 0, 0]
      intermediate_values = [ 0, 11.3, 9.5, 9.1, 8.0, 6.2]
      # Values gathered from the simulation
      final_values = [17.9, 11.3, 9.5, 9.1, 8.0, 6.2]
      bar_names = ["0", "1", "2", "3", "4", "5"]      
      # bar_names = ["0", "1", "2", "3", "4", "5",]
      chart = BarChart(
          values,
          bar_names=bar_names,
          y_range=[0, 20, 5],
          y_length=5,
          x_length=10,
          bar_fill_opacity=1,
          y_axis_config={
            "font_size": 24,
            "label_constructor": Text,   
          },
          x_axis_config={
            "font_size": 30,
            "label_constructor": Text,
            "include_ticks": False,
          },
          bar_colors=[ORANGE, GREEN]
      ).shift(UP*0)
      xLabel = chart.get_x_axis_label(Text("# idle drivers when ride is requested", font_size=30)).shift(LEFT*3.5 + DOWN*1.2)
      yLabel = chart.get_y_axis_label(Text("Average waiting time (minutes)", font_size=26)).shift(LEFT*3.8 + DOWN*3.1).rotate(90 * DEGREES)
      heading = Text("Wait times go down with more idle drivers", font_size = 40).shift(UP*3.5)
      self.add(heading)
      # axisLabels = chart.get_axis_labels(Text("Available drivers"), Text("Waiting time (minutes)"))
      self.add(chart, xLabel)
      self.add(chart, yLabel)
      self.add(chart)

      self.wait(2)

      self.play(chart.animate.change_bar_values(intermediate_values), run_time=2)
      barLabels = chart.get_bar_labels(font_size=24, label_constructor=Text)[1:]
      self.play(FadeIn(barLabels))

      self.wait(1)
      self.play(chart.animate.change_bar_values(final_values), run_time=2)
      newBarLabels = chart.get_bar_labels(font_size=24, label_constructor=Text)
      self.play(FadeIn(newBarLabels))

      self.wait(3)
    
