from manim import *

DEFAULT_FONT_SIZE = 50
Text.set_default(font="sans-serif", font_size=DEFAULT_FONT_SIZE)

class SurplusBreakdown(Scene):
    def construct(self):
      self.camera.background_color = "#444444"
      orange = ORANGE
      values=[-0.01, -0.01, 0, 0, 0, 0, -0.01]
      final_values = [-0.24,-16.07, 4.47, 4.94, 3.06, 2.61, -2.61]
      bar_names = ["Total", "Fare","Waiting time", "   Time\nsensitivity", "Income", "Substitute\n   speed",  "# passengers"]      
      chart = BarChart(
          values,
          bar_names=bar_names,
          y_range=[-15, 15, 5],
          y_length=6,
          x_length=12,
          bar_fill_opacity=1,
          y_axis_config={
            "font_size": 24,
            "label_constructor": Text,   
          },
          x_axis_config={
            "font_size": 21,
            "label_constructor": Text,
            "include_ticks": True,
          },
          bar_colors=[ORANGE, ORANGE, GREEN, GREEN, GREEN, GREEN, ORANGE]
      ).shift(UP*0)
      # yLabel = chart.get_y_axis_label(Text("Difference in surplus ($/passenger)", font_size=26)).shift(LEFT*3.3 + DOWN*3.1).rotate(90 * DEGREES)
      heading = Text("Difference in surplus ($/passenger)", font_size = 40).shift(UP*3.5)
      self.add(heading)
      # axisLabels = chart.get_axis_labels(Text("Available drivers"), Text("Waiting time (minutes)"))
      # self.add(chart, yLabel)
      self.add(chart)
      def prefix_label(text):
        if "-" not in text:
          return Text("+$" + text)
        return Text("-$" + text.replace("-", ""))
      def get_bar_labels():
        return chart.get_bar_labels(font_size=24, label_constructor=lambda text: prefix_label(text))
      self.wait(1)
      self.play(chart.animate.change_bar_values(final_values[0:1]), run_time=1)
      self.play(FadeIn(get_bar_labels()[0:1]))
      self.wait(1)
      self.play(chart.animate.change_bar_values(final_values[0:2]), run_time=1)
      self.play(FadeIn(get_bar_labels()[0:2]))
      self.wait(1)
      self.play(chart.animate.change_bar_values(final_values[0:3]), run_time=1)
      self.play(FadeIn(get_bar_labels()[0:3]))
      self.wait(1)
      self.play(chart.animate.change_bar_values(final_values[0:4]), run_time=1)
      self.play(FadeIn(get_bar_labels()[0:4]))
      self.wait(1)
      self.play(chart.animate.change_bar_values(final_values[0:5]), run_time=1)
      self.play(FadeIn(get_bar_labels()[0:5]))
      self.wait(1)
      self.play(chart.animate.change_bar_values(final_values[0:6]), run_time=1)
      self.play(FadeIn(get_bar_labels()[0:6]))
      self.wait(1)
      self.play(chart.animate.change_bar_values(final_values), run_time=1)
      self.play(FadeIn(get_bar_labels()))

      self.wait(3)
    
class TotalSurplusBreakdown(Scene):
    def construct(self):
      self.camera.background_color = "#444444"
      values=[-0.01, 0, 0, 0]
      final_values = [-332, 12600, 6000, 18300]
      bar_names = ["Passenger surplus", "Driver surplus", "Uber surplus", "Total surplus"]      
      chart = BarChart(
          values,
          bar_names=bar_names,
          y_range=[0, 20000, 5000],
          y_length=6,
          x_length=12,
          bar_fill_opacity=1,
          y_axis_config={
            "font_size": 24,
            "label_constructor": Text,   
          },
          x_axis_config={
            "font_size": 21,
            "label_constructor": Text,
            "include_ticks": True,
          },
          bar_colors=[ORANGE,GREEN, GREEN, GREEN]
      ).shift(UP*0)
      heading = Text("Difference in surplus ($)", font_size = 40).shift(UP*3.5)
      self.add(heading)
      self.add(chart)
      def prefix_label(text):
        if "-" not in text:
          return Text("+$" + text)
        return Text("-$" + text.replace("-", ""))
      def get_bar_labels():
        return chart.get_bar_labels(font_size=24, label_constructor=lambda text: prefix_label(text))
      self.wait(1)
      self.play(chart.animate.change_bar_values(final_values), run_time=1)
      self.play(FadeIn(get_bar_labels()))

      self.wait(3)